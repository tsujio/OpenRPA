CREATE USER IF NOT EXISTS 'openrpa'@'%' IDENTIFIED BY 'openrpa';
GRANT ALL ON `openrpa`.* TO 'openrpa'@'%';

CREATE DATABASE IF NOT EXISTS openrpa CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
