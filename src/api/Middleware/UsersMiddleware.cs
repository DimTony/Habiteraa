using Habitera.Data;
using Habitera.DTOs;
using Habitera.Models;
using Habitera.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Habitera.Middleware
{
    public class BlockDeletedUsersMiddleware
    {
        private readonly RequestDelegate _next;


        public BlockDeletedUsersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userId, out var userGuid))
                {
                    var user = await userManager.FindByIdAsync(userGuid.ToString());

                    if (user != null && user.Status == UserStatus.Deleted)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "This account has been deleted",
                            statusCode = 401
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}

