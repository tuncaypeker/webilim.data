namespace Webilim.Data
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System;

    public interface IData<T>
    {
        DataResult Insert(T t);
        DataResult Update(T t);
        DataResult Delete(T t);
        DataResult DeleteByKey(int id);

        T GetByKey(int id);

        List<T> GetAll();
        List<T> GetAll(string orderBy, bool isDesc = false);
        List<T> GetBy(Expression<Func<T, bool>> predicate);
        List<T> GetBy(Expression<Func<T, bool>> predicate, string orderBy, bool isDesc = false);
        List<T> GetRandom(int limit);
        List<T> GetRandom(Expression<Func<T, bool>> predicate, int limit);
        List<T> GetByPage(int pageNumber, int pageCount,string orderBy = "Id", bool isDesc = false);
        List<T> GetByPage(Expression<Func<T, bool>> predicate, int pageNumber, int pageCount, string orderBy = "Id", bool isDesc = false);

        int GetCount();
        int GetCount(Expression<Func<T, bool>> predicate);
    }
}
