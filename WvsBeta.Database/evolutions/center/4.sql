-- Add field for detecting gift read stuff
------- UP -------

ALTER TABLE itemlocker DROP COLUMN `discount_rate`, ADD COLUMN `gift_unread` tinyint NOT NULL default 0 AFTER `expiration`;

------- DOWN -------

ALTER TABLE itemlocker DROP COLUMN `gift_unread`, ADD COLUMN `discount_rate` int(11) NOT NULL AFTER `expiration`;