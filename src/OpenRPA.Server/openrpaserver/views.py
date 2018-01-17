"""
Routes and views for the flask application.
"""

import binascii
import contextlib
from datetime import datetime
import io
import json
import os
import re
import sqlite3
import uuid
import zipfile
from flask import (request, render_template, session, send_file, url_for,
                   jsonify)
from flask_socketio import SocketIO, emit, join_room, leave_room
from openrpaserver import app, socketio, db

SESSION_ID_LENGTH = 32


class Workflow(db.Model):
    id = db.Column(db.String(32), primary_key=True)
    name = db.Column(db.String(256))
    data = db.Column(db.Text())
    created_at = db.Column(db.DateTime())
    updated_at = db.Column(db.DateTime())

    def __init__(self, id, name, data, created_at, updated_at):
        self.id = id
        self.name = name
        self.data = data
        self.created_at = created_at
        self.updated_at = updated_at

    def to_json(self):
        return {
            'id': self.id,
            'name': self.name,
            'data': json.loads(self.data),
            'createdAt': "{:%Y-%m-%dT%H:%M:%SZ}".format(self.created_at),
            'updatedAt': "{:%Y-%m-%dT%H:%M:%SZ}".format(self.updated_at),
        }


class Screenshot(db.Model):
    id = db.Column(db.String(32), primary_key=True)
    image = db.Column(db.BLOB())
    created_at = db.Column(db.DateTime())

    def __init__(self, id, image, created_at):
        self.id = id
        self.image = image
        self.created_at = created_at


@app.route('/')
def home():
    """Renders the home page."""
    return render_template(
        'index.html',
        title='Home Page',
        year=datetime.now().year,
    )


@app.route('/edit')
def edit():
    """Workflow edit page."""

    session['session_id'] = binascii.hexlify(
        os.urandom(SESSION_ID_LENGTH)
    ).decode('utf-8')

    return render_template(
        'edit.html',
        title='Edit',
        year=datetime.now().year,
    )


@app.route('/screenshots/<id>', methods=['GET'])
def get_screenshot(id):
    """Get screenshot"""
    screenshot = Screenshot.query.filter_by(id=id).first()
    return send_file(io.BytesIO(screenshot.image), mimetype='image/png')


@app.route('/screenshots', methods=['POST'])
def upload_screenshot():
    """Upload screenshot"""
    token = request.args['token']
    image = request.files['screenshot']
    title = request.form['title']

    if not re.match(r'^[\w-]+$', token):
        raise Exception("Invalid token format")

    screenshot = Screenshot(
        id=str(uuid.uuid4()),
        image=image.stream.read(),
        created_at=datetime.utcnow(),
    )
    db.session.add(screenshot)
    db.session.commit()

    socket = SocketIO(message_queue=app.config['REDIS_URL'])
    socket.emit('receive screenshot', {
        'path': url_for('get_screenshot', id=screenshot.id),
        'title': title,
    }, room=token, namespace='/screenshot')

    return 'OK', 200


@socketio.on('connect', namespace='/screenshot')
def listen_screenshot():
    """Handle request for sending uploaded screenshot"""
    join_room(session['session_id'])
    emit('receiving screenshot ready')

    # TODO: leave_room at appropriate point


@app.route('/workflow/<id>', methods=['GET'])
def get_robot_file(id):
    """Get robot file"""
    workflow = Workflow.query.filter_by(id=id).first()

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, 'w', zipfile.ZIP_DEFLATED) as z:
        z.writestr('Robotfile', json.dumps({
            "version": "0.0.1",
            "id": workflow.id,
            "name": workflow.name,
            "program": "workflow.xml"
        }, indent=4).encode('utf-8'))

        data = json.dumps(json.loads(workflow.data), indent=4)
        z.writestr('workflow.xml', data.encode('utf-8'))

    buf.seek(0)
    return send_file(buf, attachment_filename='robot.zip',
                     mimetype='application/zip')


@app.route('/workflows', methods=['GET'])
def get_workflows():
    """Get workflow list"""
    workflows = Workflow.query.order_by(Workflow.name.asc()).all()
    return jsonify([w.to_json() for w in workflows])


@app.route('/workflows/<id>', methods=['GET'])
def get_workflow(id):
    """Get workflow"""
    workflow = Workflow.query.filter_by(id=id).first()
    return jsonify(workflow.to_json())


@app.route('/workflows', methods=['POST'])
def create_workflow():
    """Create workflow"""
    param = request.get_json()

    workflow = Workflow(
        id=str(uuid.uuid4()),
        name=param.get('name', 'New Workflow'),
        data=json.dumps(param.get('data', [])),
        created_at=datetime.utcnow(),
        updated_at=datetime.utcnow(),
    )
    db.session.add(workflow)
    db.session.commit()

    return jsonify({'id': workflow.id})


@app.route('/workflows/<id>', methods=['PATCH'])
def update_workflow(id):
    """Update workflow"""
    param = request.get_json()

    # TODO: Exclusive lock
    workflow = db.session.query(Workflow).filter_by(id=id).first()

    if 'name' in param:
        workflow.name = param['name']
    if 'data' in param:
        workflow.data = json.dumps(param['data'])

    db.session.add(workflow)
    db.session.commit()

    return jsonify({'id': workflow.id})


@app.route('/workflows/<id>', methods=['DELETE'])
def delete_workflow(id):
    """Delete workflow"""
    workflow = db.session.query(Workflow).filter_by(id=id).first()

    db.session.delete(workflow)
    db.session.commit()

    return jsonify()
