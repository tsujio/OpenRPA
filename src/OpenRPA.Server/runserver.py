"""
This script runs the openrpaserver application using a development server.
"""

from os import environ
from openrpaserver import app, socketio
import eventlet
eventlet.monkey_patch()


if __name__ == '__main__':
    HOST = environ.get('SERVER_HOST', 'localhost')
    try:
        PORT = int(environ.get('SERVER_PORT', '5555'))
    except ValueError:
        PORT = 5555
    socketio.run(app, host=HOST, port=PORT)
