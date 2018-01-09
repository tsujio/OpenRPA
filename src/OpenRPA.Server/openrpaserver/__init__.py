"""
The flask application package.
"""

from flask import Flask
from flask_socketio import SocketIO
from flask_redis import FlaskRedis
from . import config

app = Flask(__name__)

app.config.from_object(config.DevelopmentConfig)

socketio = SocketIO(app)
redis = FlaskRedis(app)

import openrpaserver.views
