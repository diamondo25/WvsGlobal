-- Add the table for server migration
------- UP -------

CREATE TABLE `servers` (
   `configname` varchar(45) NOT NULL,
   `world_id` tinyint(4) NOT NULL,
   `private_ip` varchar(45) DEFAULT NULL,
   PRIMARY KEY (`world_id`,`configname`)
 ) ENGINE=InnoDB DEFAULT CHARSET=latin1;
 
------- DOWN -------

DROP TABLE IF EXISTS `servers`;
