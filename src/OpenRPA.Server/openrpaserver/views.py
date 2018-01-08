"""
Routes and views for the flask application.
"""

import binascii
from datetime import datetime
import os
import re
from flask import request, render_template, session
from flask_socketio import emit
from openrpaserver import app, socketio, redis

SESSION_ID_LENGTH = 32


@app.route('/')
@app.route('/home')
def home():
    """Renders the home page."""
    return render_template(
        'index.html',
        title='Home Page',
        year=datetime.now().year,
    )


@app.route('/contact')
def contact():
    """Renders the contact page."""
    return render_template(
        'contact.html',
        title='Contact',
        year=datetime.now().year,
        message='Your contact page.'
    )


@app.route('/about')
def about():
    """Renders the about page."""
    return render_template(
        'about.html',
        title='About',
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

    if not re.match(r'^[\w-]+$', token):
        raise Exception("Invalid token format")

    channel = 'capture-' + token
    print('publish: ' + channel)
    redis.publish(channel, capture.stream.read())

    return 'OK', 200


@socketio.on('listen capture', namespace='/capture')
def listen_capture():
    """Handle request for sending uploaded capture image"""
    pubsub = redis.pubsub()
    print('subscribe: ' + 'capture-' + session['session_id'])
    pubsub.psubscribe(['capture-' + session['session_id']])
    for item in pubsub.listen():
        print(item)
        if item['type'] == 'pmessage':
            emit('receive capture', {'data': item['data']})
            pubsub.unsubscribe()
            break
