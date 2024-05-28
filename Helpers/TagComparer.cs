using WebApi.Entities;

namespace WebApi.Helpers;

public class TagComparer : IEqualityComparer<Tag>
{
    public bool Equals(Tag? x, Tag? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.TagId == y.TagId && x.TagName == y.TagName;
    }

    public int GetHashCode(Tag obj)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + obj.TagId.GetHashCode();
            hash = hash * 23 + (obj.TagName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}