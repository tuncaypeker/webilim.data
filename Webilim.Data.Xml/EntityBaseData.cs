﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Xml.Linq;
using Webilim.Core.Model;

namespace Webilim.Data.Xml
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityBaseData<T> : IData<T> where T : EntityBase
    {
        public EntityBaseData()
	    {
            //string BlogPath = ConfigurationManager.AppSettings.Get("blog:path");
	    }
        
        private static Type _type = typeof(T);
        private static string _name = _type.Name;
        private static string _folder = "data/";
        private static string _file = _folder + _name + "s.xml";
        private static string _xRoot = _name + "s";

        public DataResult Insert(T t)
        {
            t.CreateDate = DateTime.UtcNow;

            PropertyInfo[] properties = _type.GetProperties();

            //Read File
            XElement xEntitiesRoot = GetXRootEntities();
            xEntitiesRoot.Add(new XElement(_name,
                                properties
                                .Where(x => !x.GetGetMethod().IsVirtual)
                                .Select(x => new XElement(x.Name, FormatValue(x.GetValue(t, null), x.PropertyType)
                                    , new XAttribute("type", DefineType(x.PropertyType))
                             ))));
            xEntitiesRoot.Save(_file);

            ClearStartPageCache();

            var list = GetAll(); //get all clear cache add again

            return new DataResult(true, "test");
        }

        public DataResult Update(T t)
        {
            XElement currentElement = GetXRootEntities()
                .Elements(_name)
                .Where(x => ReadValue(x, "Id") == t.Id.ToString())
                .FirstOrDefault();

            if (currentElement == null)
                return new DataResult(false, "Entity Bulunamadı!");

            //bunu silelim ve yenisini ekleyelim
            DeleteByKey(t.Id);
            return Insert(t);
        }

        public DataResult Delete(T t)
        {
            return DeleteByKey(t.Id);
        }

        public DataResult DeleteByKey(int id)
        {
            XElement rootElement = GetXRootEntities();
            XElement currentElement = rootElement
                .Elements(_name)
                .Where(x => ReadValue(x, "Id") == id.ToString())
                .FirstOrDefault();

            currentElement.Remove();
            rootElement.Save(_file);

            ClearStartPageCache();

            return new DataResult(true, "Entity Silindi");
        }

        public T GetByKey(int id)
        {
            XElement currentElement = GetXRootEntities()
                .Elements(_name)
                .Where(x => ReadValue(x, "Id") == id.ToString())
                .FirstOrDefault();

            if (currentElement == null)
                return null;

            T instance = (T)Activator.CreateInstance(_type);

            PropertyInfo[] properties = _type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (null != property && property.CanWrite)
                {
                    string typeOfValue = ReadAttribute(currentElement.Element(property.Name), "type", "string");
                    string value = ReadValue(currentElement, property.Name, "");

                    switch (typeOfValue)
                    {
                        case "string": property.SetValue(instance, value, null); break;
                        case "integer": property.SetValue(instance, int.Parse(value), null); break;
                        case "datetime": property.SetValue(instance, DateTime.Parse(value), null); break;
                        default: property.SetValue(instance, value, null); break;
                    }
                }
            }

            return instance;
        }

        public List<T> GetAll()
        {
            if (HttpRuntime.Cache[_name] == null)
                LoadEntities();

            if (HttpRuntime.Cache[_name] != null)
            {
                return (List<T>)HttpRuntime.Cache[_name];
            }

            return new List<T>();
        }

        public List<T> GetAll(string orderBy, bool isDesc = false)
        {
            return isDesc
                ? GetAll().AsQueryable().Where(x => 1 == 1).OrderByDescending(orderBy).ToList()
                : GetAll().AsQueryable().Where(x => 1 == 1).OrderBy(orderBy).ToList();
        }

        public List<T> GetBy(Expression<Func<T, bool>> predicate)
        {
            return GetAll().AsQueryable().Where(predicate).ToList();
        }

        public List<T> GetBy(Expression<Func<T, bool>> predicate, string orderBy, bool isDesc = false)
        {
            return isDesc
                ? GetAll().AsQueryable().Where(predicate).OrderByDescending(orderBy).ToList()
                : GetAll().AsQueryable().Where(predicate).OrderBy(orderBy).ToList();
        }

        public List<T> GetRandom(int limit)
        {
            List<T> allList = GetAll()
                .OrderBy(x => Guid.NewGuid())
                .Take(limit)
                .ToList();
            return allList;
        }

        public List<T> GetRandom(Expression<Func<T, bool>> predicate, int limit)
        {
            List<T> allList = GetAll()
               .AsQueryable()
               .Where(predicate)
               .OrderBy(x => Guid.NewGuid())
               .Take(limit)
               .ToList();
            return allList;
        }

        public List<T> GetByPage(int pageNumber, int pageCount, string orderBy = "Id", bool isDesc = false)
        {
            try
            {
                return isDesc
                    ? GetAll().AsQueryable().Where(x=>1==1).OrderByDescending(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList()
                    : GetAll().AsQueryable().Where(x => 1 == 1).OrderBy(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList();
            }
            catch
            {
                return null;
            }
        }

        public List<T> GetByPage(Expression<Func<T, bool>> predicate, int pageNumber, int pageCount, string orderBy = "Id", bool isDesc = false)
        {
            try
            {
                return isDesc
                    ? GetAll().AsQueryable().Where(predicate).OrderByDescending(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList()
                    : GetAll().AsQueryable().Where(predicate).OrderBy(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList();
            }
            catch
            {
                return null;
            }
        }

        public int GetCount()
        {
            return GetAll().Count;
        }

        public int GetCount(Expression<Func<T, bool>> predicate)
        {
            return GetAll()
                .AsQueryable()
                .Where(predicate)
                .Count();
        }

        #region Helper Methods

        private static void LoadEntities()
        {
            XElement xEntitiesRoot = GetXRootEntities();

            IEnumerable<XElement> xEntities = xEntitiesRoot.Elements(_name);
            List<T> list = new List<T>();
            // Can this be done in parallel to speed it up?
            foreach (XElement xEntity in xEntities)
            {
                T instance = (T)Activator.CreateInstance(_type);

                PropertyInfo[] properties = _type.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    //virtual ise, ve bununla iliskili propertyId var ise relation ifade etmeli
                    if (property.GetGetMethod().IsVirtual && properties.Any(x=>x.Name.Equals(property.Name+"Id")))
                    {
                        Type propertyType = property.PropertyType;
                        Type repositoryType = typeof(EntityBaseData<>).MakeGenericType(propertyType);

                        object repository = Activator.CreateInstance(repositoryType);
                        MethodInfo method = repositoryType.GetMethod("GetByKey");

                        //get property Value
                        PropertyInfo propertyRelation = properties
                                .Where(x => x.Name.Equals(property.Name + "Id"))
                                .FirstOrDefault();
                        object toto 
                            = method.Invoke(repository, new object[] { propertyRelation.GetValue(instance,null) });

                        property.SetValue(instance, toto, null);

                        continue;
                    }

                    //virtual ile isaretli olan varmi
                    if (null != property && property.CanWrite)
                    {
                        string typeOfValue = ReadAttribute(xEntity.Element(property.Name), "type", "string");
                        string value = ReadValue(xEntity, property.Name, "");

                        switch (typeOfValue)
                        {
                            case "string": property.SetValue(instance, value, null); break;
                            case "integer": property.SetValue(instance, int.Parse(value), null); break;
                            case "datetime": property.SetValue(instance, DateTime.Parse(value), null); break;
                            default: property.SetValue(instance, value, null); break;
                        }
                    }
                }

                list.Add(instance);
            }

            if (list.Count > 0)
            {
                list.Sort((p1, p2) => p2.CreateDate.CompareTo(p1.CreateDate));
                HttpRuntime.Cache.Insert(_name, list);
            }
        }

        private static XElement GetXRootEntities()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            if (!File.Exists(_file))
            {
                XElement rootElement = new XElement(_xRoot);
                rootElement.Save(_file);
            }

            //Get xml file into XElement
            XElement xEntitiesRoot = XElement.Load(_file);

            return xEntitiesRoot;
        }

        private static string ReadValue(XElement doc, XName name, string defaultValue = "")
        {
            if (doc.Element(name) != null)
                return doc.Element(name).Value;

            return defaultValue;
        }

        private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
        {
            if (element.Attribute(name) != null)
                return element.Attribute(name).Value;

            return defaultValue;
        }

        private string FormatValue(object value, Type type)
        {
            if (type.Equals(typeof(string)))
                return value.ToString();
            else if (type.Equals(typeof(int)))
                return value.ToString();
            else if (type.Equals(typeof(DateTime)))
                return ((DateTime)value).ToString("yyyy-MM-dd HH:m:ss");
            else
                return value.ToString();
        }

        private string DefineType(Type type)
        {
            if (type.Equals(typeof(string)))
                return "string";
            else if (type.Equals(typeof(int)))
                return "integer";
            else if (type.Equals(typeof(DateTime)))
                return "datetime";
            else
                return "string";
        }

        public static void ClearStartPageCache()
        {
            HttpRuntime.Cache.Remove(_name);
        }

        #endregion
    }
}
