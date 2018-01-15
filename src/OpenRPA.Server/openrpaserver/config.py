import os


class BaseConfig:
    pass


class DevelopmentConfig:
    SECRET_KEY = 'OpenRPA'
    REDIS_URL = "redis://{}:{}/0".format(
        os.environ['REDIS_PORT_6379_TCP_ADDR'],
        os.environ['REDIS_PORT_6379_TCP_PORT']
    )
