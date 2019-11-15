declare @defaultSchema varchar(100) = 'dbo';

select schema_name(tab.schema_id) as [schema_name], 
    tab.[name] as table_name,
	'Table' [obj_type]
into #tableSchemas
from sys.tables tab
where schema_name(tab.schema_id) not in (@defaultSchema, 'HangFire')


insert into #tableSchemas
select schema_name(v.schema_id) as [schema_name],
       v.name as view_name,
	   'View' [obj_type]
from sys.views v 
where schema_name(v.schema_id) <> @defaultSchema
order by schema_name,
         view_name


select [obj_type],schema_name,[table_name],
'modelBuilder.Entity<'+table_name+'>().ToTable("'+table_name+'", "'+[schema_name]+'");'
from #tableSchemas k1
order by [obj_type],schema_name,[table_name]

select [obj_type],schema_name,[table_name],
'modelBuilder.Ignore<'+table_name+'>();'
from #tableSchemas k1
order by [obj_type],schema_name,[table_name]

drop table #tableSchemas