namespace Webilim.Data.EF
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Linq.Dynamic;
    using Webilim.Core.Model;

    public class EntityBaseData<T> : IData<T> where T : EntityBase
    {
        protected readonly DbContext _context;

        public EntityBaseData(DbContext context)
        {
            _context = context;
        }

        protected virtual void BeforeUpdate() { }
        protected virtual void AfterUpdate() { }
        protected virtual void BeforeInsert() { }
        protected virtual void AfterInsert() { }
        protected virtual void BeforeDelete() { }
        protected virtual void AfterDelete() { }

        #region IData Implementation

        /// <summary>
        /// Insert new Entity
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public DataResult Insert(T t)
        {
            try
            {
                BeforeInsert();
                _context.Set<T>().Add(t);
                AfterInsert();

                _context.SaveChanges();

                return new DataResult(true, "");
            }
            catch (Exception exc)
            {
                return new DataResult(false, exc.Message +
                    exc.InnerException == null ? "" : "(" + exc.InnerException + ")"
                );
            }
        }

        /// <summary>
        /// Update Entity
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public DataResult Update(T t)
        {
            try
            {
                int updateId = t.Id;

                T aModel = _context.Set<T>().Where(x => x.Id == updateId).FirstOrDefault();

                if (aModel == null)
                    return new DataResult(false, "Güncelleme yapılacak kayıt bulunamıyor");

                BeforeUpdate();

                foreach (var propertyInfo in typeof(T).GetProperties())
                    propertyInfo.SetValue(aModel, propertyInfo.GetValue(t, null), null);

                AfterUpdate();

                _context.SaveChanges();

                return new DataResult(true, "");
            }
            catch (Exception exc)
            {
                return new DataResult(false, exc.Message +
                    exc.InnerException == null ? "" : "(" + exc.InnerException + ")"
                );
            }
        }

        /// <summary>
        /// Delete Entity
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public DataResult Delete(T t)
        {
            return DeleteByKey(t.Id);
        }

        /// <summary>
        /// Delete Entity by Key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataResult DeleteByKey(int id)
        {
            try
            {
                T aModel = _context.Set<T>().Where(x => x.Id == id).FirstOrDefault();

                if (aModel == null)
                    return new DataResult(false, "Silinecek kayıt bulunamıyor");

                BeforeDelete();
                _context.Set<T>().Remove(aModel);
                AfterDelete();
                _context.SaveChanges();

                return new DataResult(true, "");
            }
            catch (Exception exc)
            {
                return new DataResult(false, exc.Message +
                    exc.InnerException == null ? "" : "(" + exc.InnerException + ")"
                );
            }
        }

        /// <summary>
        /// Get Entity By Key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetByKey(int id)
        {
            try
            {
                T aModel = _context.Set<T>().Where(x => x.Id == id).FirstOrDefault();
                return aModel;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get All Entities
        /// </summary>
        /// <returns></returns>
        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        /// <summary>
        /// Get All Entities
        /// </summary>
        /// <param name="orderBy">property name to order</param>
        /// <returns></returns>
        public List<T> GetAll(string orderBy, bool isDesc = false)
        {
            return isDesc
                ? _context.Set<T>().OrderByDescending(orderBy).ToList()
                : _context.Set<T>().OrderBy(orderBy).ToList();
        }
        
        /// <summary>
        /// Find an Entity
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<T> GetBy(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _context.Set<T>()
                    .Where(predicate)
                    .ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Find an Entity
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy">order by property name</param>
        /// <returns></returns>
        public List<T> GetBy(Expression<Func<T, bool>> predicate, string orderBy, bool isDesc = false)
        {
            try
            {
                return isDesc
                    ? _context.Set<T>().Where(predicate).OrderByDescending(orderBy).ToList()
                    : _context.Set<T>().Where(predicate).OrderBy(orderBy).ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get Entities by Page
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public List<T> GetByPage(int pageNumber, int pageCount, string orderBy = "Id", bool isDesc = false)
        {
            try
            {
                return isDesc
                    ? _context.Set<T>().OrderByDescending(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList()
                    : _context.Set<T>().OrderBy(orderBy).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Find and Get Entities by Page
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        public List<T> GetByPage(Expression<Func<T, bool>> predicate, int pageNumber, int pageCount, string orderBy = "Id", bool isDesc = false)
        {
            try
            {
                return isDesc
                   ? _context.Set<T>().OrderByDescending(orderBy).Where(predicate).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList()
                   : _context.Set<T>().OrderBy(orderBy).Where(predicate).Skip((pageNumber - 1) * pageCount).Take(pageCount).ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get Count of Entities
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            return _context.Set<T>().Count();
        }

        /// <summary>
        /// Find and Get Count of Entities
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int GetCount(Expression<Func<T, bool>> predicate)
        {
            return _context.Set<T>()
                .Where(predicate)
                .Count();
        }

        /// <summary>
        /// Get random records 
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public List<T> GetRandom(int limit)
        {
            if (limit <= 0)
                return new List<T>();

            return _context.Set<T>().OrderBy(x => Guid.NewGuid()).Take(limit).ToList();
        }

        /// <summary>
        /// Get random records in a predicate
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public List<T> GetRandom(Expression<Func<T, bool>> predicate, int limit)
        {
            if (limit <= 0)
                return new List<T>();

            return _context.Set<T>()
                .Where(predicate)
                .OrderBy(x => Guid.NewGuid())
                .Take(limit).ToList();
        }

        #endregion
    }
}
