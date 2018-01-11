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
import zipfile
from flask import request, render_template, session, send_file, url_for
from flask_socketio import emit
from openrpaserver import app, socketio, redis

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

    channel = 'capture-' + token
    print('publish: ' + channel)
    redis.publish(channel, json.dumps({
        'capture_id': capture_id,
        'title': title,
    }))

    return 'OK', 200


@app.route('/capture/<id>', methods=['GET'])
def get_capture(id):
    """Get uploaded window capture"""
    with contextlib.closing(sqlite3.connect(SQLITE_FILE)) as conn:
        c = conn.cursor()

        c.execute("SELECT id, data FROM captures WHERE id = ?", (id,))

        row = c.fetchone()

    return send_file(io.BytesIO(row[1]), mimetype='image/png')


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
            data = json.loads(item['data'])
            emit('receive capture', {
                'title': data['title'],
                'path': url_for('get_capture', id=data['capture_id']),
            })
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
