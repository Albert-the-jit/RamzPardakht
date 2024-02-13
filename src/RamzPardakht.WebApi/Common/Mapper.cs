// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.Models;
using Riok.Mapperly.Abstractions;

namespace RamzPardakht.WebApi.Common;
[Mapper]
public partial class Mapper
{
    public partial ReferenceToken ToEntity(ReferenceTokenModel model);
    public partial Payment ToEntity(PaymentCreationRequestModel model);
    public partial ReferenceTokenModel ToModel(ReferenceToken entity);
    public partial PaymentInquiryResponseModel ToModel(Payment entity);
    public partial PayoutCreationResponseModel ToModel(Payout entity);
}

[Mapper]
public static partial class StaticMapper
{
    public static partial IQueryable<ReferenceTokenModel> ProjectToModel(this IQueryable<ReferenceToken> organizations);
    public static partial IQueryable<PayoutReportModel> ProjectToModel(this IQueryable<Payout> payouts);
    [MapProperty(nameof(Payment.Id),nameof(PaymentReportModel.RefId))]
    public static partial IQueryable<PaymentReportModel> ProjectToModel(this IQueryable<Payment> payouts);


    [MapProperty(nameof(Payment.Id),nameof(PaymentReportModel.RefId))]
    private static partial PaymentReportModel ToModel(this Payment entity);

}
