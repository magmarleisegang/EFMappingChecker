
select schema_name(tab.schema_id) as [schema_name], 
   -- pk.[name] as pk_name,
    ic.index_column_id as column_id,
    col.[name] as column_name, 
    tab.[name] as table_name,
	'Table' as [obj_type]
	into #keysTable
from sys.tables tab
    inner join sys.indexes pk
        on tab.object_id = pk.object_id 
        and pk.is_primary_key = 1
    inner join sys.index_columns ic
        on ic.object_id = pk.object_id
        and ic.index_id = pk.index_id
    inner join sys.columns col
        on pk.object_id = col.object_id
        and col.column_id = ic.column_id
where schema_name(tab.schema_id) <> 'HangFire'

insert into #keysTable
select schema_name(v.schema_id) as schema_name,
       c.column_id,
       c.name as column_name,
       v.name as view_name,
	   'View' as [obj_type]
from sys.columns c
join sys.views v 
     on v.object_id = c.object_id
order by schema_name,
         view_name,
         column_id;


--SELECT STRING_AGG( ISNULL(column_name, ' '), ',') From keysTable
--select 
--[obj_type],table_name,
--'modelBuilder.Entity<'+table_name+'>().HasKey(x=> new { ',
--(SELECT STRING_AGG( 'x.'+ISNULL(column_name, ' '), ',') From #keysTable k2 where k2.table_name = k1.table_name ) 
--, ' });'
--from #keysTable k1
--group by [obj_type],k1.table_name
--order by [obj_type],table_name

select [obj_type], table_name from #keysTable
group by [obj_type],table_name
order by table_name
drop table #keysTable