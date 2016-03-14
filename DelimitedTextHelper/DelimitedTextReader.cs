using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DelimitedTextHelper
{
	public class DelimitedTextReader : IDisposable
	{
		private DelimitedTextParser _parser;
		private string[] _currentRecord;
		private string[] _headerRecord;
		private int _currentIndex = -1;
		private bool _doneReading;
		private bool _hasBeenRead;
		
        public virtual bool IgnoreMappingExceptions { get; set; }
		public virtual bool AreFieldHeadersCaseSensitive { get; set; }
		public virtual bool FirstRowIsHeader { get; set; }
		/// <summary>
		/// Gets the field headers.
		/// </summary>
		public virtual string[] FieldHeaders
		{
			get
			{
				throwIfParserHasNotBeenRead();
				return _headerRecord;
			}
		}

		/// <summary>
		/// Get the current record;
		/// </summary>
		public virtual string[] CurrentRecord
		{
			get
			{
				throwIfParserHasNotBeenRead();
				return _currentRecord;
			}
		}
		
		public DelimitedTextReader(TextReader reader):this(reader, ',')
		{
		}

		public DelimitedTextReader(TextReader reader, char delimiter)
		{
			_parser = new DelimitedTextParser(reader, delimiter);
		}
				
		public virtual bool Read()
		{
			//CheckDisposed();

			if (_doneReading)
			{
				return false;
			}

			if (FirstRowIsHeader && _headerRecord == null)
			{
				do
				{
					_currentRecord = _parser.Read();
				}
				while (SkipRecord());
				_headerRecord = _currentRecord;
				_currentRecord = null;
				ParseNamedIndexes();
			}

			do
			{
				_currentRecord = _parser.Read();
			}
			while (SkipRecord());

			_currentIndex = -1;
			_hasBeenRead = true;

			if (_currentRecord == null)
			{
				_doneReading = true;
			}

			return _currentRecord != null;
		}

		public TEntity GetRecord<TEntity>() where TEntity : new()
		{
			throwIfParserHasNotBeenRead();
			TEntity record;
			try
			{
				record = createRecord<TEntity>();
			}
			catch (Exception)
			{

				throw;
			}
			return record;
		}

        private List<PropertyMapping> _propertyMappings = new List<PropertyMapping>();
		public PropertyMapping MapProperty<TRecord>(Expression<Func<TRecord, object>> expression)
		{
            var property = GetProperty<TRecord>(expression);

            var propertyMap = new PropertyMapping(property)
            {
                MappedColumnIndex = getMaxIndex() + 1
            };

            _propertyMappings.Add(propertyMap);

            return propertyMap;
		}

		public bool SkipRecord()
		{
			return false;
		}

		public void ParseNamedIndexes()
		{

		}

		public void Dispose()
		{
			
		}

        private int getMaxIndex()
        {
            if(_propertyMappings.Count == 0)
            {
                return -1;
            }

            var indexes = new List<int>();
            if(_propertyMappings.Count > 0)
            {
                indexes.Add(_propertyMappings.Max(pm => pm.MappedColumnIndex));
            }
            return indexes.Max();
        }

		private TEntity createRecord<TEntity>() where TEntity:new()
		{
			TEntity record;
			try
			{
				record = new TEntity();
                //try to auto map the header names to properties on the class
                //first get all of the property infos for the type:
                AutoGeneratePropertyMappings<TEntity>();

                foreach (var pm in _propertyMappings)
                {
                    int index = pm.MappedColumnIndex;
                    if(pm.UseColumnName && FirstRowIsHeader)
                    {
                        index = Array.FindIndex(FieldHeaders, t => t.Equals(pm.MappedColumnName, StringComparison.InvariantCultureIgnoreCase));
                        if(index == -1)
                        {
                            if (!IgnoreMappingExceptions)
                            {
                                string message = string.Format("Mapping exception occurred.  The property '{0}' could not be mapped to column '{1}'", pm.PropertyInfo.Name, pm.MappedColumnName);
                                throw new Exception();
                            }
                            index = pm.MappedColumnIndex;
                        }
                    }
                    //var value = Convert.ChangeType(CurrentRecord[index], pm.PropertyInfo.PropertyType);
                    if(pm.TypeConverter != null)
                    {
                        var value = pm.TypeConverter.ConvertFromString(CurrentRecord[index]);
                        pm.PropertyInfo.SetValue(record, value);
                    }                    
                }
				
				return record;
			}
			catch (Exception)
			{

				throw;
			}
			
		}

		//private Dictionary<string, PropertyInfo> _propertyMappings = new Dictionary<string, PropertyInfo>();
		private void AutoGeneratePropertyMappings<TRecord>()
		{
            if (_propertyMappings.Count == 0)
            {
                var properties = typeof(TRecord).GetProperties().Where(x => x.PropertyType.Module.ScopeName == "CommonLanguageRuntimeLibrary").ToArray();
                if (FirstRowIsHeader)
                {
                    AutoGeneratePropertyMappingsByName<TRecord>(properties);
                    //get the propertied that were not mapped
                    var headers = FieldHeaders.ToList<string>();                    
                    var mappedNames = _propertyMappings.Select(n => n.MappedColumnName).ToList();
                    foreach (var property in properties)
                    {                        
                        var existingPM = _propertyMappings.Where(m => m.PropertyInfo == property).FirstOrDefault();
                        if (existingPM != null)
                        {
                            continue;
                        }

                        for (int i = 0; i < FieldHeaders.Length; i++)
                        {
                            if (mappedNames.Contains(FieldHeaders[i]))
                            {
                                continue;
                            }

                            PropertyMapping pm = new PropertyMapping(property)
                            {
                                MappedColumnIndex = i,
                                MappedColumnName = FieldHeaders[i],
                            };

                            mappedNames.Add(FieldHeaders[i]);
                            _propertyMappings.Add(pm);
                            break;
                        }
                    }
                }
                else
                {
                    AutoGeneratePropertyMappingsByIndex<TRecord>(properties);
                }
            }
		}

        private void AutoGeneratePropertyMappingsByIndex<TRecord>(PropertyInfo[] properties)
        {
            
            foreach (var property in properties)
            {
                int index = getMaxIndex() + 1;

                if (index < CurrentRecord.Length)
                {
                    PropertyMapping pm = new PropertyMapping(property)
                    {
                        MappedColumnIndex = index,
                        MappedColumnName = FirstRowIsHeader ? FieldHeaders[index] : string.Empty
                    };

                    _propertyMappings.Add(pm);
                }
            }
        }

        private void AutoGeneratePropertyMappingsByName<TRecord>(PropertyInfo[] properties)
        {            
            foreach (var property in properties)
            {
                
                int index = Array.FindIndex(FieldHeaders, t => t.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));

                if (index == -1)
                {                        
                    continue;
                }
                PropertyMapping pm = new PropertyMapping(property)
                {
                    MappedColumnIndex = index,
                    MappedColumnName = FieldHeaders[index]
                };

                _propertyMappings.Add(pm);
            }
        }

		private PropertyInfo GetProperty<T>( string propertyName)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			if (!AreFieldHeadersCaseSensitive)
			{
				flags = flags | BindingFlags.IgnoreCase;
			}
			PropertyInfo pi = typeof(T).GetProperty(propertyName, flags);
			return pi;
		}

		private void throwIfParserHasNotBeenRead()
		{
			if (!_hasBeenRead)
			{
				throw new Exception("Read must be invoked before data can bee accessed.");
			}
		}

		private PropertyInfo GetProperty<TRecord>(Expression<Func<TRecord, object>> expression)
		{
			var member = GetMemberExpression(expression).Member;
			var property = member as PropertyInfo;
			if(property == null)
			{
				throw new Exception(string.Format("'{0}' is not a property."));
			}

			return property;
		}

		private MemberExpression GetMemberExpression<TModel, T>(Expression<Func<TModel, T>> expression)
		{
			// This method was taken from FluentNHibernate.Utils.ReflectionHelper.cs and modified.
			// http://fluentnhibernate.org/

			MemberExpression memberExpression = null;
			if (expression.Body.NodeType == ExpressionType.Convert)
			{
				var body = (UnaryExpression)expression.Body;
				memberExpression = body.Operand as MemberExpression;
			}
			else if (expression.Body.NodeType == ExpressionType.MemberAccess)
			{
				memberExpression = expression.Body as MemberExpression;
			}

			if (memberExpression == null)
			{
				throw new ArgumentException("Not a member access", "expression");
			}

			return memberExpression;
		}
	}

	public class PropertyMapping
	{
		public PropertyInfo PropertyInfo { get; set; }
		public string MappedColumnName { get; set; }
		public int MappedColumnIndex { get; set; }
		public bool UseColumnName { get; set; }
        public TypeConverter TypeConverter { get; set; }
        public PropertyMapping ColumnName(string name)
        {
            MappedColumnName = name;
            UseColumnName = true;
            return this;
        } 

        public PropertyMapping(PropertyInfo property)
        {
            PropertyInfo = property;
            TypeConverter = TypeDescriptor.GetConverter(property.DeclaringType);
        }
	}
}
