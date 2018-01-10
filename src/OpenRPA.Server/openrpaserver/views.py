"""
Routes and views for the flask application.
"""

import binascii
from datetime import datetime
import io
import json
import os
import pickle
import re
import zipfile
from flask import request, render_template, session, send_file
from flask_socketio import emit
from openrpaserver import app, socketio, redis

SESSION_ID_LENGTH = 32


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


@app.route('/capture', methods=['POST'])
def capture():
    """Window capture upload handler."""
    token = request.args['token']
    capture = request.files['capture']
    title = request.form['title']

    if not re.match(r'^[\w-]+$', token):
        raise Exception("Invalid token format")

    channel = 'capture-' + token
    print('publish: ' + channel)
    redis.publish(channel, pickle.dumps({
        'capture': capture.stream.read(),
        'title': title,
    }))

    return 'OK', 200


@socketio.on('listen capture', namespace='/capture')
def listen_capture():
    """Handle request for sending uploaded capture image"""
    pubsub = redis.pubsub()
    print('subscribe: ' + 'capture-' + session['session_id'])
    pubsub.subscribe(['capture-' + session['session_id']])

    emit('ready receiving capture')

    for item in pubsub.listen():
        print(item)
        if item['type'] == 'message':
            emit('receive capture', pickle.loads(item['data']))
            pubsub.unsubscribe()
            break


@app.route('/download', methods=['POST'])
def download():
    capture = request.files['capture']
    rect = json.loads(request.form['rect'])
    title = request.form['title']

    buf = io.BytesIO()
    with zipfile.ZipFile(buf, 'w', zipfile.ZIP_DEFLATED) as z:
        z.writestr('Robotfile', json.dumps({
            "version": "0.0.1",
            "id": "2ae54a21-9605-4fb4-a980-222b87578493",
            "name": "sample1",
            "program": "workflow.xml"
        }).encode('utf-8'))

        z.writestr('workflow.xml', """
        <?xml version="1.0" ?>
        <Workflow>
          <ImageMatch windowTitle="{title}" imagePath="{rect}" matchAction="LeftClick" />
        </Workflow>
        """.format(rect=rect, title=title).encode('utf-8'))

        z.writestr('image.png', capture.read())

    buf.seek(0)
    return send_file(buf, attachment_filename='robot.zip', mimetype='application/zip')
