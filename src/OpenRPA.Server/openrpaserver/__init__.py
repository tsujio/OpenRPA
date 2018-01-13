"""
The flask application package.
"""

from flask import Flask
from flask_socketio import SocketIO
from . import config


class CustomFlask(Flask):
    """Custom version of changing Jinja template engine delimiters"""
    jinja_options = Flask.jinja_options.copy()
    jinja_options.update(dict(
        block_start_string='(%',
        block_end_string='%)',
        variable_start_string='((',
        variable_end_string='))',
        comment_start_string='(#',
        comment_end_string='#)',
    ))


app = CustomFlask(__name__)

app.config.from_object(config.DevelopmentConfig)

socketio = SocketIO(app, message_queue=app.config['REDIS_URL'])

import openrpaserver.views
