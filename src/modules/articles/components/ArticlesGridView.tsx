import { Card, CardContent } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useTranslation } from "react-i18next";
import { Edit, Eye, MoreVertical, Package, Trash2, ArrowRightLeft } from "lucide-react";
import { getStatusColor, getStatusIcon } from "./utils";

export function ArticlesGridView({
  items,
  onView,
  onEdit,
  onTransfer,
}: {
  items: any[];
  onView: (item: any) => void;
  onEdit: (item: any) => void;
  onTransfer: (item: any) => void;
}) {
  const { t } = useTranslation('articles');
  return (
    <div className="p-3 sm:p-4 lg:p-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 sm:gap-6">
        {items.map((article) => {
          const StatusIcon = getStatusIcon(article.status);
          return (
            <Card 
              key={article.id} 
              className="shadow-card border-0 hover-lift transition-all duration-200 bg-card group cursor-pointer"
              onClick={() => onView(article)}
            >
              <CardContent className="p-4 sm:p-6">
                <div className="flex items-start justify-between mb-3 sm:mb-4">
                  <Avatar className="h-10 w-10 sm:h-12 sm:w-12">
                    <AvatarFallback className="text-xs sm:text-sm bg-primary/10 text-primary">
                      <Package className="h-4 w-4 sm:h-6 sm:w-6" />
                    </AvatarFallback>
                  </Avatar>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button 
                        variant="ghost" 
                        size="sm" 
                        className="h-8 w-8 p-0"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <MoreVertical className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem 
                        className="gap-2"
                        onClick={() => onView(article)}
                      >
                        <Eye className="h-4 w-4" />
                        {t("actions.view_details")}
                      </DropdownMenuItem>
                      <DropdownMenuItem 
                        className="gap-2"
                        onClick={() => onEdit(article)}
                      >
                        <Edit className="h-4 w-4" />
                        {t("actions.edit_article")}
                      </DropdownMenuItem>
                      <DropdownMenuItem 
                        className="gap-2"
                        onClick={() => onTransfer(article)}
                      >
                        <ArrowRightLeft className="h-4 w-4" />
                        {t("actions.transfer")}
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem className="gap-2 text-destructive">
                        <Trash2 className="h-4 w-4" />
                        {t("actions.delete")}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
                
                <div className="mb-3 sm:mb-4">
                  <h3 className="font-semibold text-foreground text-sm sm:text-base mb-1 truncate">{article.name}</h3>
                  <p className="text-xs sm:text-sm text-muted-foreground mb-2 truncate">{t("fields.sku")}: {article.sku} - {article.category}</p>
                  <Badge className={`${getStatusColor(article.status)} text-xs`}>
                    <StatusIcon className="h-3 w-3 mr-1" />
                    {article.status.replace("_", " ")}
                  </Badge>
                </div>
                
                <div className="space-y-2 text-xs sm:text-sm text-muted-foreground">
                  <div className="flex justify-between">
                    <span>{t("fields.stock")}:</span>
                    <span className="font-medium">{article.stock} {t("fields.units")}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>{t("fields.price")}:</span>
                    <span className="font-medium">{article.sellPrice} TND</span>
                  </div>
                  <div className="flex justify-between">
                    <span>{t("fields.location")}:</span>
                    <span className="font-medium truncate">{article.location}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
