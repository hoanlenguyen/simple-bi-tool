select count(1) from customer ;

select count(1) from customerscore c  ;

create table TestJob(
   Id INT NOT NULL AUTO_INCREMENT,
   CreationTime DATETIME,
   Detail VARCHAR(200), 
   PRIMARY KEY ( Id )
);
