FROM python:3.6

COPY ./requirements.txt /app/requirements.txt

RUN pip install -r /app/requirements.txt

ENV SERVER_HOST=0.0.0.0

ENV SERVER_PORT=80

COPY ./runserver.py /app/runserver.py

COPY ./openrpaserver /app/openrpaserver

CMD [ "python", "/app/runserver.py" ]
