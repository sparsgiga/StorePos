using StorePos.Domain.Interfaces;

namespace StorePos.Domain.Aggregates.User;

public interface IUserRepository :
    IRepository<User, long>,
    IQueryRepository<User, long>;
