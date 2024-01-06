// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.Models;
using Riok.Mapperly.Abstractions;

namespace RamzPardakht.WebApi.Common;
[Mapper]
public partial  class Mapper
{
    public partial ReferenceToken ToEntity(ReferenceTokenModel referenceTokenModel);
    public partial ReferenceTokenModel ToModel(ReferenceToken referenceToken);
}

[Mapper]
public static partial class StaticMapper
{
    public static partial IQueryable<ReferenceTokenModel> ProjectToModel(this IQueryable<ReferenceToken> organizations);
}
