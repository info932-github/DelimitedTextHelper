using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private TEntity createRecord<TEntity>() where TEntity:new()
        {
            TEntity record;
            try
            {
                record = new TEntity();
                if (FirstRowIsHeader)
                {
                    //try to auto map the header names to properties on the class
                    //first get all of the property infos for the type:
                    buildPropertyMappings<TEntity>();
                    for(int i = 0; i < _headerRecord.Length;i++)
                    {
                        var item = _headerRecord[i];                        
                        if(_propertyMappings.ContainsKey(item))
                        {
                            var value = Convert.ChangeType(CurrentRecord[i], _propertyMappings[item].PropertyType);                           
                            _propertyMappings[item].SetValue(record, value);
                        }
                    }
                }
                return record;
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        private Dictionary<string, PropertyInfo> _propertyMappings = new Dictionary<string, PropertyInfo>();
        private void buildPropertyMappings<TRecord>()
        {
            if (FirstRowIsHeader)
            {
                //try to auto map the header names to properties on the class
                //first get all of the property infos for the type:
                var propertyCount = typeof(TRecord).GetRuntimeProperties().Count();
                
                for (int i = 0; i < _headerRecord.Length; i++)
                {
                    var item =  _headerRecord[i];
                    var prop = GetProperty<TRecord>(item);
                    if (prop != null)
                    {
                        _propertyMappings.Add(_headerRecord[i], prop);
                    }
                }                
            }
            else
            {
                //build property map by index.  map according to the property name in order it appears
                var properties = typeof(TRecord).GetRuntimeProperties();
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
    }
}
