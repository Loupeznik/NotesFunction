using DZarsky.CommonLibraries.AzureFunctions.Configuration;
using DZarsky.CommonLibraries.AzureFunctions.Security;
using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

using User = DZarsky.CommonLibraries.AzureFunctions.Models.Users.User;

namespace DZarsky.NotesFunction.Services
{
    public sealed class UserService
    {
        private readonly CosmosClient _db;
        private readonly PasswordUtils _passwordUtils;
        private readonly CosmosConfiguration _config;

        public UserService(CosmosClient db, PasswordUtils utils, CosmosConfiguration config)
        {
            _db = db;
            _passwordUtils = utils;
            _config = config;
        }

        public async Task<UserInfoResult> CreateUser(UserDto credentials)
        {
            var container = GetContainer();

            var userByLogin = container
                .GetItemLinqQueryable<User>()
                .Where(x => x.Login == credentials.Login)
                .ToFeedIterator();

            if ((await userByLogin.ReadNextAsync()).Any())
            {
                return new UserInfoResult(ResultStatus.AlreadyExists);
            }

            var hashedPassword = _passwordUtils.HashPassword(credentials.Password);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Login = credentials.Login,
                Password = hashedPassword,
                DateCreated = DateTime.Now
            };

            var response = await container.CreateItemAsync(user);

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                return new UserInfoResult(ResultStatus.Failed);
            }

            response.Resource.Password = null;

            return new UserInfoResult(ResultStatus.Success, response.Resource);
        }

        public async Task<UserInfoResult> GetInfo(UserDto credentials)
        {
            var container = GetContainer();

            var userByLogin = container
                .GetItemLinqQueryable<User>()
                .Where(x => x.Login == credentials.Login)
                .ToFeedIterator();

            var user = (await userByLogin.ReadNextAsync()).FirstOrDefault();

            if (user == null || !_passwordUtils.ValidatePassword(credentials.Password, user.Password))
            {
                return new UserInfoResult(ResultStatus.NotFound);
            }

            user.Password = null;

            return new UserInfoResult(ResultStatus.Success, user);
        }

        private Container GetContainer()
        {
            return _db.GetContainer(_config.DatabaseID, _config.UsersContainerID);
        }
    }
}
