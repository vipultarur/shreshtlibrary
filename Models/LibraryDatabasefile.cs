using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryDatabasefile
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public byte[] Data { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
