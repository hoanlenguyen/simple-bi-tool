create table AdminScore(
   ScoreID INT NOT NULL AUTO_INCREMENT,
   ScoreCategory VARCHAR(200),
   ScoreTitle VARCHAR(500),
   Points INT NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME,
   INDEX(ScoreCategory),
   INDEX(ScoreTitle),
   PRIMARY KEY ( ScoreID )
);
-- create INDEX ScoreCatergory ON AdminScore (ScoreCatergory);
-- DROP INDEX ScoreCatergory ON AdminScore;
-- SHOW INDEX FROM AdminScore
--ALTER TABLE AdminScore CHANGE ScoreCatergory ScoreCategory VARCHAR(200);

create table AdminCampaign(
   CampaignID INT NOT NULL AUTO_INCREMENT,
   CampaignName VARCHAR(500),
   CampaignDate DATETIME,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME,
   INDEX(CampaignName),
   PRIMARY KEY ( CampaignID )
);


create table Customer(
   CustomerID INT NOT NULL AUTO_INCREMENT,
   DateFirstAdded DATETIME NOT NULL,
   CustomerMobileNo VARCHAR(20) NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME, 
   UNIQUE INDEX (CustomerMobileNo),
   PRIMARY KEY ( CustomerID )
);


create table CustomerScore(
   CustomerScoreID INT NOT NULL AUTO_INCREMENT,
   CustomerMobileNo VARCHAR(10) NOT NULL,
   ScoreID INT NOT NULL,
   DateOccurred DATETIME NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME, 
   PRIMARY KEY ( CustomerScoreID )
);

create table RecordCustomerExport(
   ID INT NOT NULL AUTO_INCREMENT,
   CustomerMobileNo VARCHAR(10) NOT NULL,
   CampaignNameID INT NOT NULL,
   DateExported DATETIME NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME, 
   PRIMARY KEY (ID)
);

