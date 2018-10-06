------- UP -------

ALTER TABLE users 
ADD COLUMN max_unique_id_ban_count TINYINT(1) DEFAULT 5,
ADD COLUMN max_ip_ban_count TINYINT(1) DEFAULT 3,
ADD COLUMN banned_by VARCHAR(13) DEFAULT NULL AFTER ban_reason_message,
ADD COLUMN banned_at DATETIME DEFAULT NULL AFTER banned_by;

------- DOWN -------

ALTER TABLE users
DROP COLUMN max_unique_id_ban_count, 
DROP COLUMN max_ip_ban_count, 
DROP COLUMN banned_by, 
DROP COLUMN banned_at;