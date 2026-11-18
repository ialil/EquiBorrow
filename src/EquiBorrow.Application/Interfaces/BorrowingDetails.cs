using System;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public sealed record BorrowingDetails(
    int BorrowingId,
    int StudentId,
    string StudentNumber,
    string StudentName,
    int EquipmentId,
    string EquipmentIdentifier,
    string EquipmentName,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    DateOnly? ReturnDate,
    BorrowingStatus Status,
    uint Version);
