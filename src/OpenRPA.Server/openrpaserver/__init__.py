"""
The flask application package.
"""

from flask import Flask
from flask_socketio import SocketIO
from flask_redis import FlaskRedis

app = Flask(__name__)

app.secret_key = 'TODO: change secret key'
app.config['REDIS_URL'] = "redis://192.168.254.130:6379/0"

socketio = SocketIO(app)
redis = FlaskRedis(app)

import openrpaserver.views
