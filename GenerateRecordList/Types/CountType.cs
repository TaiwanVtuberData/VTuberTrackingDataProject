namespace GenerateRecordList.Types;
public class BaseCountType {
    public CountTag tag { get; protected set; }
    // TODO: Move count to HasCountType because it's only valid when that is the case
    public ulong? count { get; protected set; }
};

public class HasCountType : BaseCountType {
    public HasCountType(ulong _count) {
        tag = CountTag.has;
        count = _count;
    }
}

public class HiddenCountType : BaseCountType {
    public HiddenCountType() {
        tag = CountTag.hidden;
        count = null;
    }
}

public class NoCountType : BaseCountType {
    public NoCountType() {
        tag = CountTag.no;
        count = null;
    }
}
