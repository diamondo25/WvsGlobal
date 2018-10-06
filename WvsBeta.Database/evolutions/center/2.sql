-- Add last_savepoint column to detect speed levelling and such

------- UP -------

ALTER TABLE `characters` 
ADD COLUMN `last_savepoint` DATETIME NULL DEFAULT '2012-01-09 12:37:00' AFTER `party`;

------- DOWN -------

ALTER TABLE `characters` 
DROP COLUMN `last_savepoint`;

