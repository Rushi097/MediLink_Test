-- Optional production hardening. Run as a MySQL administrator.
CREATE USER IF NOT EXISTS 'medilink_auth'@'localhost' IDENTIFIED BY 'CHANGE_ME_AUTH_PASSWORD';
CREATE USER IF NOT EXISTS 'medilink_order'@'localhost' IDENTIFIED BY 'CHANGE_ME_ORDER_PASSWORD';
GRANT ALL PRIVILEGES ON MediLinkAuth.* TO 'medilink_auth'@'localhost';
GRANT ALL PRIVILEGES ON MediLinkOrder.* TO 'medilink_order'@'localhost';
FLUSH PRIVILEGES;
