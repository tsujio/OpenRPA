class BaseConfig:
    pass


class DevelopmentConfig:
    SECRET_KEY = 'OpenRPA'
    REDIS_URL = "redis://localhost:6379/0"
