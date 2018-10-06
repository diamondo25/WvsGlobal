-- Add column for EULA
------- UP -------

ALTER TABLE users ADD COLUMN `confirmed_eula` TINYINT(1) NOT NULL DEFAULT '0';

------- DOWN -------

ALTER TABLE users DROP COLUMN `confirmed_eula`;
