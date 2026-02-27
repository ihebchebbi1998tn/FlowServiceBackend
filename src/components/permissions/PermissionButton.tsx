import { Button, ButtonProps } from "@/components/ui/button";
import { usePermissions } from "@/hooks/usePermissions";
import { PermissionModule, PermissionAction } from "@/types/permissions";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

interface PermissionButtonProps extends ButtonProps {
  module: PermissionModule;
  action: PermissionAction;
  hideWhenDisabled?: boolean;
  tooltipWhenDisabled?: string;
}

/**
 * A button that is disabled or hidden based on user permissions.
 * Use this for create, edit, delete actions throughout the app.
 */
export function PermissionButton({
  module,
  action,
  hideWhenDisabled = false,
  tooltipWhenDisabled = "You don't have permission to perform this action",
  children,
  disabled,
  ...props
}: PermissionButtonProps) {
  const { isMainAdmin, hasPermission, isLoading } = usePermissions();

  const hasAccess = isMainAdmin || hasPermission(module, action);
  const isDisabled = disabled || isLoading || !hasAccess;

  // Hide completely if no permission and hideWhenDisabled is true
  if (hideWhenDisabled && !hasAccess && !isLoading) {
    return null;
  }

  // If disabled due to permission, show tooltip
  if (!hasAccess && !isLoading) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <span>
              <Button {...props} disabled={true}>
                {children}
              </Button>
            </span>
          </TooltipTrigger>
          <TooltipContent>
            <p>{tooltipWhenDisabled}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  return (
    <Button {...props} disabled={isDisabled}>
      {children}
    </Button>
  );
}

export default PermissionButton;
