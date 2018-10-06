------- UP -------

ALTER TABLE users ADD last_unique_id VARCHAR(26) DEFAULT '';
ALTER TABLE users CHANGE donator donator TINYINT(1) DEFAULT 0;

ALTER TABLE machine_ban ADD last_unique_id VARCHAR(26) DEFAULT '';

UPDATE users SET donator = 0;


------- DOWN -------

ALTER TABLE users DROP last_unique_id;
ALTER TABLE users CHANGE donator donator INT(10) DEFAULT 1;

ALTER TABLE machine_ban DROP last_unique_id;

UPDATE users SET donator = 1;

