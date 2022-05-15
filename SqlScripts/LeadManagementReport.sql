USE devdb; 
create table LeadManagementReport(
   ID INT NOT NULL AUTO_INCREMENT,
   CustomerMobileNo VARCHAR(10),
   DateFirstAdded DATETIME NOT NULL,
   TotalTimesExported INT NOT null,
   DateLastExported DATETIME,
   LastUsedCampaignID INT,
   SecondLastUsedCampaign INT,
   ThirdLastUsedCampaign INT,
   DateLastOccurred DATETIME,
   TotalResultPoints NOT null,
   
   INDEX(CustomerMobileNo),
   PRIMARY KEY ( ID )
);