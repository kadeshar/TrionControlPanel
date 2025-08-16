-- Create the database if it doesn't exist
CREATE DATABASE IF NOT EXISTS `TrionAPI`;
USE `TrionAPI`;

-- Create a new MySQL user (replace 'trion_user' and 'StrongPassword123!' as needed)
CREATE USER IF NOT EXISTS 'trion_user'@'localhost' IDENTIFIED BY 'StrongPassword123!';
GRANT ALL PRIVILEGES ON `TrionAPI`.* TO 'trion_user'@'localhost';
FLUSH PRIVILEGES;

-- Create table for download counts
CREATE TABLE IF NOT EXISTS `DownloadCount` (
    `ID` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(255) NOT NULL,
    `UID` VARCHAR(255) NOT NULL,
    `Count` INT DEFAULT 0,
    UNIQUE KEY `unique_name_uid` (`Name`, `UID`)
);

-- Create table for supporter keys
CREATE TABLE IF NOT EXISTS `SupporterKey` (
    `ID` INT AUTO_INCREMENT PRIMARY KEY,
    `ApiKey` VARCHAR(255) NOT NULL UNIQUE,
    `UID` BIGINT NOT NULL
);
