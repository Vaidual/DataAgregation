# DataAggregation
---
A .NET Core Console application for aggregation data about game events.

The application parses json data and fills the databases. All db access operations runs in multithreading, and all data are separated between different databases. 
Variety of queries allows to get data for statistic, about levels, revenue, DAU and etc. ExcelHandler allows to save data of custom format in xlsx.
Also available some functionallity for clustering users by income and thier ages.

It`s primary made for a lab work.
