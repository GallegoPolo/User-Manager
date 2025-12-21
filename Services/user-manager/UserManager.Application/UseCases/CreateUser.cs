using System;
using System.Collections.Generic;
using System.Text;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases
{
    public class CreateUser
    {
        private readonly IUserRepository _repository;

        public CreateUser(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task ExecuteAsync(string name, string email)
        {
            var user = new User(name, email);
            await _repository.AddAsync(user);
        }

    }
}
