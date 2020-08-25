using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using UsersManager.Infrastructure;
using UsersManager.Models;

namespace UsersManager.Controllers
{
	[RoutePrefix("api/accounts")]
	public class AccountsController : BaseApiController
	{
		[Authorize]
		[Route("users")]
		public IHttpActionResult GetUsers()
		{
			return Ok(this.AppUserManager.Users.ToList().Select(u => this.TheModelFactory.Create(u)));
		}

		[Authorize]
		[Route("user/{id:guid}", Name = "GetUserById")]
		public async Task<IHttpActionResult> GetUser(string Id)
		{
			var user = await this.AppUserManager.FindByIdAsync(Id);

			if (user != null)
			{
				return Ok(this.TheModelFactory.Create(user));
			}

			return NotFound();

		}

		[Authorize]
		[Route("user/{username}")]
		public async Task<IHttpActionResult> GetUserByName(string username)
		{
			var user = await this.AppUserManager.FindByNameAsync(username);

			if (user != null)
			{
				return Ok(this.TheModelFactory.Create(user));
			}

			return NotFound();
		}

		[AllowAnonymous]
		[Route("create")]
		public async Task<IHttpActionResult> CreateUser(CreateUserBindingModel createUserModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var user = new ApplicationUser()
			{
				UserName = createUserModel.Username,
				Email = createUserModel.Email,
				FirstName = createUserModel.FirstName,
				LastName = createUserModel.LastName,
				Level = 3,
				JoinDate = DateTime.Now.Date,
			};

			IdentityResult addUserResult = await this.AppUserManager.CreateAsync(user, createUserModel.Password);

			if (!addUserResult.Succeeded)
			{
				return GetErrorResult(addUserResult);
			}

			string code = await this.AppUserManager.GenerateEmailConfirmationTokenAsync(user.Id);

			var callbackUrl = new Uri(Url.Link("ConfirmEmailRoute", new { userId = user.Id, code = code }));

			await this.AppUserManager.SendEmailAsync(user.Id, "CONFIRMA TU CUENTA", "Por favor confirma tu cuenta dando clic <a href=\"" + callbackUrl + "\"> aquí</a>");

			Uri locationHeader = new Uri(Url.Link("GetUserById", new { id = user.Id }));

			return Created(locationHeader, TheModelFactory.Create(user));
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("ReSendTokenNewAccount")]
		public async Task<IHttpActionResult> ReSendTokenNewAccount(string userName, string password)
		{
			var user = await this.AppUserManager.FindAsync(userName, password);

			if (user != null)
			{
				if (!user.EmailConfirmed)
				{
					string code = await this.AppUserManager.GenerateEmailConfirmationTokenAsync(user.Id);

					var callbackUrl = new Uri(Url.Link("ConfirmEmailRoute", new { userId = user.Id, code = code }));

					await this.AppUserManager.SendEmailAsync(user.Id, "CONFIRMA TU CUENTA", "Por favor confirma tu cuenta dando clic <a href=\"" + callbackUrl + "\">aquí</a>");

					return Ok("Correo enviado");
				}
				else
				{
					throw new Exception("No existe correo para confirmar del usuario: " + userName);
				}
			}
			else
			{
				throw new Exception("El nombre de usuario o contraseña es incorrecto");
			}
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("ConfirmEmail", Name = "ConfirmEmailRoute")]
		public async Task<IHttpActionResult> ConfirmEmail(string userId = "", string code = "")
		{
			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
			{
				ModelState.AddModelError("error", "ID de usuario y código es requerido.");
				return BadRequest(ModelState);
			}

			IdentityResult result = await this.AppUserManager.ConfirmEmailAsync(userId, code);

			if (result.Succeeded)
			{
				await this.AppUserManager.SendEmailAsync(userId, "BIENVENIDO A PAMBOLEROS", "Tu cuenta ha sido creada correctamente. ¡¡¡ Bienvenido !!!");
				return Ok("Bienvenido");
			}
			else
			{
				return GetErrorResult(result);
			}
		}

		[Authorize]
		[Route("ChangePassword")]
		public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			IdentityResult result = await this.AppUserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);

			if (!result.Succeeded)
			{
				return GetErrorResult(result);
			}

			return Ok();
		}

		[AllowAnonymous]
		[HttpPost]
		[Route("ForgotPassword")]
		public async Task<IHttpActionResult> ForgotPassword(ForgotPasswordBindingModel model)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var user = await this.AppUserManager.FindByEmailAsync(model.Email);
			if (user == null || !(await this.AppUserManager.IsEmailConfirmedAsync(user.Id)))
			{
				// Don't reveal that the user does not exist or is not confirmed
				throw new Exception("Información inválida");
			}

			string code = await this.AppUserManager.GeneratePasswordResetTokenAsync(user.Id);

			var callbackUrl = new Uri(Url.Link("ResetPasswordRoute", new { userId = user.Id, code = code, newPassword = model.NewPassword }));

			await this.AppUserManager.SendEmailAsync(user.Id, "RESETEA TU CONTRASEÑA", "Termina el reseteo de tu contraseña dando clic <a href=\"" + callbackUrl + "\">aquí</a>");

			return Ok("Correo enviado");
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("ResetPassword", Name = "ResetPasswordRoute")]
		public async Task<IHttpActionResult> ResetPassword(string userId, string code, string newPassword)
		{
			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
			{
				ModelState.AddModelError("error", "ID de usuario, código y contraseña son requeridos.");
				return BadRequest(ModelState);
			}

			IdentityResult result = await this.AppUserManager.ResetPasswordAsync(userId, code, newPassword);

			if (result.Succeeded)
			{
				await this.AppUserManager.SendEmailAsync(userId, "CONTRASEÑA MODIFICADA", "Tu contraseña ha sido modificada correctamente. ¡¡¡ No la pierdas otra vez por favor !!!");
				return Ok("Contraseña modificada");
			}
			else
			{
				return GetErrorResult(result);
			}
		}

		[Authorize(Roles = "SuperAdmin")]
		[Route("user/{id:guid}")]
		public async Task<IHttpActionResult> DeleteUser(string id)
		{

			//Only SuperAdmin or Admin can delete users (Later when implement roles)

			var appUser = await this.AppUserManager.FindByIdAsync(id);

			if (appUser != null)
			{
				IdentityResult result = await this.AppUserManager.DeleteAsync(appUser);

				if (!result.Succeeded)
				{
					return GetErrorResult(result);
				}

				return Ok();

			}

			return NotFound();
		}

		[Authorize]
		[Route("user/roles/{id:guid}")]
		[HttpPost]
		public async Task<IHttpActionResult> AssignRolesToUser([FromUri] string id, [FromBody] string[] rolesToAssign)
		{

			var appUser = await this.AppUserManager.FindByIdAsync(id);

			if (appUser == null)
			{
				return NotFound();
			}

			var currentRoles = await this.AppUserManager.GetRolesAsync(appUser.Id);

			var rolesNotExists = rolesToAssign.Except(this.AppRoleManager.Roles.Select(x => x.Name)).ToArray();

			if (rolesNotExists.Count() > 0)
			{
				ModelState.AddModelError("error", string.Format("Roles '{0}' no existen en el sistema.", string.Join(",", rolesNotExists)));
				return BadRequest(ModelState);
			}

			IdentityResult removeResult = await this.AppUserManager.RemoveFromRolesAsync(appUser.Id, currentRoles.ToArray());

			if (!removeResult.Succeeded)
			{
				ModelState.AddModelError("error", "Ocurrio un error al quitar los roles de usuario.");
				return BadRequest(ModelState);
			}

			IdentityResult addResult = await this.AppUserManager.AddToRolesAsync(appUser.Id, rolesToAssign);

			if (!addResult.Succeeded)
			{
				ModelState.AddModelError("error", "Ocurrio un error al añadir los roles de usuario.");
				return BadRequest(ModelState);
			}

			return Ok();
		}

		[Authorize]
		[Route("user/assignclaims/{id:guid}")]
		[HttpPost]
		public async Task<IHttpActionResult> AssignClaimsToUser([FromUri] string id, [FromBody] List<ClaimBindingModel> claimsToAssign)
		{

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var appUser = await this.AppUserManager.FindByIdAsync(id);

			if (appUser == null)
			{
				return NotFound();
			}

			foreach (ClaimBindingModel claimModel in claimsToAssign)
			{
				if (appUser.Claims.Any(c => c.ClaimType == claimModel.Type))
				{

					await this.AppUserManager.RemoveClaimAsync(id, ExtendedClaimsProvider.CreateClaim(claimModel.Type, claimModel.Value));
				}

				await this.AppUserManager.AddClaimAsync(id, ExtendedClaimsProvider.CreateClaim(claimModel.Type, claimModel.Value));
			}

			return Ok();
		}

		[Authorize]
		[Route("user/removeclaims/{id:guid}")]
		[HttpPost]
		public async Task<IHttpActionResult> RemoveClaimsFromUser([FromUri] string id, [FromBody] List<ClaimBindingModel> claimsToRemove)
		{

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var appUser = await this.AppUserManager.FindByIdAsync(id);

			if (appUser == null)
			{
				return NotFound();
			}

			foreach (ClaimBindingModel claimModel in claimsToRemove)
			{
				if (appUser.Claims.Any(c => c.ClaimType == claimModel.Type))
				{
					await this.AppUserManager.RemoveClaimAsync(id, ExtendedClaimsProvider.CreateClaim(claimModel.Type, claimModel.Value));
				}
			}

			return Ok();
		}
	}
}