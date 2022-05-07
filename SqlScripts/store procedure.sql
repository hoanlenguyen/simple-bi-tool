DROP PROCEDURE IF EXISTS `SearchCustomer`

CREATE PROCEDURE `SearchCustomer`(
IN scoreId int = null,
IN scoreCategory varchar(200)= null,
IN keyWord varchar(200)= null,
IN dateFirstAddedFrom DATETIME = null,
IN dateFirstAddedTo DATETIME = null,
)
BEGIN
        SET @query = CONCAT('SELECT count(cID) 
        FROM tblclassifieds c
        JOIN tblphotos p 
        ON c.cmd5val=p.cClassCode 
        WHERE p.cpMain=1 
        AND c.cMarkedInappropriate=0 
        AND c.cBlacklisted=0 
        AND c.cEndDate>NOW() 
        AND (cType=29) OR (c.cType=27 OR c.cType=28) 
        AND c.cCompanyId IN (',_dealerIds,')
        AND (("',_dealerPhoneNumber,'" is null) or (c.cPhoneNum="',_dealerPhoneNumber,'"));');
    --  SELECT @query;
     PREPARE stmt FROM @query;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END //