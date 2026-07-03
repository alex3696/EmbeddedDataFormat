using EdfNet.Core;

namespace EdfNet;

public interface IWriter
{
    void Write(Config cfg);
    void Write(Schema sch);
    EdfErr Write(object obj);
    void Flush();
}
