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
from openrpaserver import app, socketio

SESSION_ID_LENGTH = 32

SQLITE_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           'app.db')

# Create db (TODO: use other rdb)
if not os.path.exists(SQLITE_FILE):
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()
        c.execute("""
        CREATE TABLE captures (
            id CHAR(64),
            data BLOB
        );
        """)

        c.execute("""
        CREATE TABLE workflows (
            id CHAR(64),
            workflow TEXT
        );
        """)


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


@app.route('/capture/<id>', methods=['GET'])
def get_capture(id):
    """Get uploaded window capture"""
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()

        c.execute("SELECT id, data FROM captures WHERE id = ?", (id,))

        row = c.fetchone()

    return send_file(io.BytesIO(row[1]), mimetype='image/png')


@app.route('/capture', methods=['POST'])
def upload_capture():
    """Window capture upload handler."""
    token = request.args['token']
    capture = request.files['capture']
    title = request.form['title']

    if not re.match(r'^[\w-]+$', token):
        raise Exception("Invalid token format")

    capture_id = binascii.hexlify(
        os.urandom(SESSION_ID_LENGTH)
    ).decode('utf-8')
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()

        c.execute("INSERT INTO captures(id, data) VALUES (?, ?)",
                  (capture_id, capture.stream.read()))

        conn.commit()

    print(app.config['REDIS_URL'])
    socket = SocketIO(message_queue=app.config['REDIS_URL'])
    socket.emit('receive capture', {
        'path': url_for('get_capture', id=capture_id),
        'title': title,
    }, room=token, namespace='/capture')

    return 'OK', 200


@socketio.on('connect', namespace='/capture')
def listen_capture():
    """Handle request for sending uploaded capture image"""
    join_room(session['session_id'])
    emit('receiving capture ready')

    # TODO: leave_room at appropriate point


@app.route('/workflow/save', methods=['POST'])
def save_workflow():
    """Save workflow"""
    workflow = request.get_json()

    id = binascii.hexlify(os.urandom(SESSION_ID_LENGTH)).decode('utf-8')
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()

        c.execute("INSERT INTO workflows(id, workflow) VALUES (?, ?)",
                  (id, json.dumps(workflow)))

        conn.commit()

    return jsonify({'id': id})


@app.route('/workflow/<id>', methods=['GET'])
def get_workflow(id):
    """Get workflow"""
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()

        c.execute("SELECT id, workflow FROM workflows WHERE id = ?", (id,))

        row = c.fetchone()

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, 'w', zipfile.ZIP_DEFLATED) as z:
        z.writestr('Robotfile', json.dumps({
            "version": "0.0.1",
            "id": str(uuid.uuid4()),
            "name": "sample1",
            "program": "workflow.xml"
        }, indent=4).encode('utf-8'))

        z.writestr('workflow.xml', row[1].encode('utf-8'))

    buf.seek(0)
    return send_file(buf, attachment_filename='robot.zip',
                     mimetype='application/zip')
