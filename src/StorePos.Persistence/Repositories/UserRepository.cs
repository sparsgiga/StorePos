using StorePos.Domain.Aggregates.User;
using StorePos.Persistence.Context;

namespace StorePos.Persistence.Repositories;

public sealed class UserRepository(StorePosDbContext context)
    : Repository<User, long>(context), IUserRepository;
