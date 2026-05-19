using System.Text.Json.Serialization;

namespace LoanApplication.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoanStatus { Pending, Approved, Rejected };
