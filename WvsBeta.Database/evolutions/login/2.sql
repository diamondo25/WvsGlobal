-- Record Machine ID in users table
------- UP -------

ALTER TABLE `users` 
ADD COLUMN `last_machine_id` VARCHAR(32) NULL AFTER `last_ip`;

------- DOWN -------

ALTER TABLE `users` DROP COLUMN `last_machine_id`;
