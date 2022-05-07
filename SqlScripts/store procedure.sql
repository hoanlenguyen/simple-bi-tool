USE devdb; 
DROP PROCEDURE IF EXISTS `SearchCustomer`;
DELIMITER $$
CREATE PROCEDURE `SearchCustomer`(
IN scoreId INT,
IN scoreCategory varchar(200),
IN keyWord varchar(200),
IN dateFirstAddedFrom DATETIME,
IN dateFirstAddedTo DATETIME
)
BEGIN
    SELECT c.CustomerMobileNo 
    FROM customer c     
    INNER JOIN customerscore cs 
    ON c.CustomerMobileNo = cs.CustomerMobileNo
    LEFT JOIN adminscore ads 
    ON ads.ScoreID = cs.ScoreID 
    where ((scoreId is not null and cs.ScoreID = scoreId) OR scoreId is null)
    and ((scoreCategory is not null and ads.ScoreCategory = scoreCategory) OR scoreCategory is null)
    and ((keyWord is not null and c.CustomerMobileNo like CONCAT('%', keyWord, '%')) OR keyWord is null)
    and ((dateFirstAddedFrom is not null and c.DateFirstAdded >= dateFirstAddedFrom) OR dateFirstAddedFrom is null)
    and ((dateFirstAddedTo is not null and c.DateFirstAdded <= dateFirstAddedTo) OR dateFirstAddedTo is null)
   ;
END$$
DELIMITER