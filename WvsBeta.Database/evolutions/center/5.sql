------- UP -------

ALTER TABLE cashitem_pet DROP COLUMN charid;

------- DOWN -------


ALTER TABLE cashitem_pet ADD COLUMN charid INT(11) NOT NULL AFTER userid;
