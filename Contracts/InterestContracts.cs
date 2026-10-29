namespace InvestCase.API.Contracts;

public record CreateInterestRequest(Guid FromUserId, Guid ToUserId);
