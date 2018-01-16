#!/bin/sh

# TODO: use docker-compose

PORT=80

OPENRPA_NAME=openrpa

MYSQL_NAME=openrpa-mysql
MYSQL_DATADIR=~/OpenRPA/mysql-data
MYSQL_ROOT_PASSWORD=mysql
MYSQL_VERSION=5.7

REDIS_NAME=openrpa-redis
REDIS_VERSION=3.2

# TODO: remove
SQLITE_DATA=~/OpenRPA/openrpa.db

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ $DEBUG == '1' ]; then
    extra_args="-e FLASK_DEBUG=1 -v $script_dir/..:/app"
fi

if [ $# -gt 0 ]; then
    PORT=$1
fi

build_arg=""
if [ $# -gt 1 ]; then
    # e.g.: HTTPS_PROXY=proxy.example.com
    build_arg="--build-arg $2"
fi

docker stop $OPENRPA_NAME
docker stop $MYSQL_NAME
docker stop $REDIS_NAME

# Build application
docker build \
       -t $OPENRPA_NAME \
       $build_arg \
       $script_dir/../ \
    || exit 1

# Start MySQL
docker run \
       --name $MYSQL_NAME \
       --rm \
       -v $MYSQL_DATADIR:/var/lib/mysql \
       -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD \
       -d \
       mysql:$MYSQL_VERSION \
       || exit 1

# Wait for mysql up
sleep 3

# Create database
docker run \
       -it \
       --link $MYSQL_NAME:mysql \
       --rm \
       -v $script_dir/setup.sql:/setup.sql \
       -e MYSQL_PWD=$MYSQL_ROOT_PASSWORD \
       mysql:$MYSQL_VERSION \
       sh -c 'exec mysql -h"$MYSQL_PORT_3306_TCP_ADDR" \
                         -P"$MYSQL_PORT_3306_TCP_PORT" \
                         -uroot < /setup.sql' \
       || exit 1

# Start Redis
docker run \
       --name $REDIS_NAME \
       --rm \
       -d \
       redis:$REDIS_VERSION \
       || exit 1

# Start application
docker run \
       --name $OPENRPA_NAME \
       --link $MYSQL_NAME:mysql \
       --link $REDIS_NAME:redis \
       --rm \
       -p $PORT:80 \
       -d \
       -v $SQLITE_DATA:/var/lib/OpenRPA/openrpa.db \
       $extra_args \
       $OPENRPA_NAME \
       || exit 1

echo "running on $PORT"
