USE devdb; 
DROP PROCEDURE IF EXISTS `SearchCustomer`;
DELIMITER $$
CREATE PROCEDURE `SearchCustomer`(
IN scoreIds varchar(200),
IN scoreCategory varchar(200),
IN keyWord varchar(200),
IN dateFirstAddedFrom DATETIME,
IN dateFirstAddedTo DATETIME
)
BEGIN
    select distinct cs.CustomerMobileNo 
    from customerscore cs
    where ((scoreIds is not null and cs.ScoreID in (scoreIds)) OR scoreIds is null)    
    and ((keyWord is not null and cs.CustomerMobileNo like CONCAT('%', keyWord, '%')) OR keyWord is null)
    and ((dateFirstAddedFrom is not null and cs.DateOccurred  >= dateFirstAddedFrom) OR dateFirstAddedFrom is null)
    and ((dateFirstAddedTo is not null and cs.DateOccurred <= dateFirstAddedTo) OR dateFirstAddedTo is null)
   ;
END$$
DELIMITER

call SearchCustomer(null,'1,2,3,4',null,null,null,null);
