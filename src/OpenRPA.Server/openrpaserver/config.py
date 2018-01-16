import os


class BaseConfig:
    pass


class DevelopmentConfig:
    SECRET_KEY = 'OpenRPA'

    REDIS_URL = "redis://{}:{}/0".format(
        os.environ.get('REDIS_PORT_6379_TCP_ADDR', 'localhost'),
        os.environ.get('REDIS_PORT_6379_TCP_PORT', 6379)
    )

    SQLALCHEMY_DATABASE_URI = 'sqlite:////var/lib/OpenRPA/openrpa.db'
    SQLALCHEMY_TRACK_MODIFICATIONS = False
