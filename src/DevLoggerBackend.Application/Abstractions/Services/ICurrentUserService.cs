using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevLoggerBackend.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}
