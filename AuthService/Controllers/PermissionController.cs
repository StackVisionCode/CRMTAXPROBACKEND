using AuthService.DTOs.PermissionDTOs;
using Commands.PermissionCommands;
using Common;
using MediatR;
//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.PermissionQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(IMediator mediator, ILogger<PermissionController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Crear un nuevo permiso
        /// </summary>
        [HasPermission("Permission.Create")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] PermissionDTO permissionDto
        )
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state for permission creation: {@ModelState}",
                    ModelState
                );
                return BadRequest(ModelState);
            }

            var command = new CreatePermissionCommands(permissionDto);
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Actualizar un permiso existente
        /// </summary>
        [HasPermission("Permission.Update")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(
            Guid id,
            [FromBody] PermissionDTO permissionDto
        )
        {
            if (id != permissionDto.Id)
            {
                _logger.LogWarning(
                    "Route ID {RouteId} does not match body ID {BodyId}",
                    id,
                    permissionDto.Id
                );
                return BadRequest("Route ID must match permission ID in body");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state for permission update: {@ModelState}",
                    ModelState
                );
                return BadRequest(ModelState);
            }

            var command = new UpdatePermissionCommands(permissionDto);
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Eliminar un permiso
        /// </summary>
        [HasPermission("Permission.Delete")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeletePermissionCommands(id);
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtener todos los permisos
        /// </summary>
        // [HasPermission("Permission.Read")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PermissionDTO>>>> GetAll()
        {
            var query = new GetAllPermissionQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Obtener un permiso por ID
        /// </summary>
        // [HasPermission("Permission.Read")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<PermissionDTO>>> GetById(Guid id)
        {
            var query = new GetPermissionByIdQuery(id);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Obtener permisos efectivos de un usuario (roles + custom permissions)
        /// </summary>
        [HttpGet("user/{userId:guid}/effective")]
        public async Task<
            ActionResult<ApiResponse<UserPermissionsDTO>>
        > GetUserEffectivePermissions(Guid userId)
        {
            var query = new GetUserPermissionsQuery(userId);
            var result = await _mediator.Send(query);

            if (!(result?.Success ?? false))
                return NotFound(result?.Message ?? "Permissions not found");

            return Ok(result);
        }

        /// <summary>
        /// Obtener solo códigos de permisos (para compatibilidad)
        /// </summary>
        [HttpGet("user/{userId:guid}/codes")]
        public async Task<ActionResult<List<string>>> GetUserPermissionCodes(Guid userId)
        {
            var query = new GetUserPermissionsQuery(userId);
            var result = await _mediator.Send(query);

            if (!(result?.Success ?? false))
                return NotFound(result?.Message ?? "Permissions not found");

            return Ok(result!.Data!.PermissionCodes);
        }

        /// <summary>
        /// Obtener permisos por categoría/módulo
        /// </summary>
        // [HasPermission("Permission.Read")]
        [HttpGet("by-category/{category}")]
        public async Task<ActionResult<ApiResponse<List<PermissionDTO>>>> GetByCategory(
            string category
        )
        {
            var query = new GetAllPermissionQuery();
            var result = await _mediator.Send(query);

            if (result.Success ?? false && result.Data != null)
            {
                var filteredPermissions = result
                    .Data!.Where(p =>
                        p.Code.StartsWith($"{category}.", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                return Ok(
                    new ApiResponse<List<PermissionDTO>>(
                        true,
                        $"Permissions for category {category} retrieved successfully",
                        filteredPermissions
                    )
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Obtener permisos disponibles para asignación personalizada
        /// Excluye permisos del sistema que no deberían ser asignados manualmente
        /// </summary>
        // [HasPermission("Permission.Read")]
        [HttpGet("assignable")]
        public async Task<ActionResult<ApiResponse<List<PermissionDTO>>>> GetAssignablePermissions()
        {
            var query = new GetAllPermissionQuery();
            var result = await _mediator.Send(query);

            if (result.Success ?? false && result.Data != null)
            {
                // Filtrar permisos que NO deberían ser asignados manualmente
                var systemPrefixes = new[] { "Permission.", "Role.", "RolePermission." };

                var assignablePermissions = result
                    .Data!.Where(p =>
                        !systemPrefixes.Any(prefix =>
                            p.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .ToList();

                return Ok(
                    new ApiResponse<List<PermissionDTO>>(
                        true,
                        "Assignable permissions retrieved successfully",
                        assignablePermissions
                    )
                );
            }

            return BadRequest(result);
        }
    }
}
