using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DZarsky.NotesFunction.Services
{
    public sealed class NoteService
    {
        private readonly CosmosClient _db;

        public NoteService(CosmosClient db)
        {
            _db = db;
        }

        public async Task<GenericResult<NoteDto>> Create(NoteDto note)
        {
            throw new NotImplementedException();
        }

        public async Task<GenericResult<NoteDto>> Update(NoteDto note)
        {
            throw new NotImplementedException();
        }

        public async Task<GenericResult<IList<NoteDto>>> List()
        {
            throw new NotImplementedException();
        }

        public async Task<GenericResult<NoteDto>> Get(string? id)
        {
            throw new NotImplementedException();
        }

        public async Task<GenericResult> Delete(string? id)
        {
            throw new NotImplementedException();
        }
    }
}
