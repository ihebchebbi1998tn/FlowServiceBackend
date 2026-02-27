import { useState } from "react";
import { ArrowLeft, Save, Package, Zap } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";
import { FastAddForm } from "../components/FastAddForm";

const categories = [
  "Tools",
  "Electrical",
  "Plumbing", 
  "Safety",
  "Hardware",
  "Materials",
  "Equipment"
];

const locations = [
  "Warehouse A",
  "Warehouse B", 
  "Service Van 1",
  "Service Van 2",
  "Main Office"
];

const AddArticle = () => {
  const { t } = useTranslation('articles');
  const navigate = useNavigate();
  const { toast } = useToast();
  const [showFastForm, setShowFastForm] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    sku: "",
    description: "",
    category: "",
    location: "",
    stock: "",
    minStock: "",
    price: "",
    sellPrice: "",
    supplier: "",
    notes: ""
  });

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      // Here you would typically save to Supabase
      console.log("Saving article:", formData);
      
      toast({
        title: t("add.success"),
        description: t("add.success_message"),
      });
      
      navigate("/dashboard/articles");
    } catch (error) {
      const _err = error as any;
      toast({
        title: t("add.error"),
        description: t("add.error_message"),
        variant: "destructive",
      });
    }
  };

  const isFormValid = formData.name && formData.sku && formData.category && formData.location;

  return (
    <div className="space-y-6 p-4 sm:p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/dashboard/articles">
            <ArrowLeft className="h-4 w-4 mr-2" />
            {t("detail.back_to_articles")}
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-foreground">{t("add.title")}</h1>
          <p className="text-muted-foreground">{t("add.subtitle")}</p>
        </div>
        <Button 
          variant="outline" 
          onClick={() => setShowFastForm(!showFastForm)}
          className="gap-2"
        >
          <Zap className="h-4 w-4" />
          {showFastForm ? t("add.detailed_form") : t("add.quick_add")}
        </Button>
      </div>

      {/* Fast Form Option */}
      {showFastForm && (
        <div>
          <FastAddForm 
            onSuccess={() => navigate("/dashboard/articles")}
            onSwitchToDetailed={() => setShowFastForm(false)}
          />
        </div>
      )}

      {!showFastForm && (

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Basic Information */}
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Package className="h-5 w-5" />
                {t("add.basic_information")}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="name">{t("add.article_name")} *</Label>
                  <Input
                    id="name"
                    value={formData.name}
                    onChange={(e) => handleInputChange("name", e.target.value)}
                    placeholder={t("add.article_name_placeholder")}
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="sku">{t("fields.sku")} *</Label>
                  <Input
                    id="sku"
                    value={formData.sku}
                    onChange={(e) => handleInputChange("sku", e.target.value)}
                    placeholder={t("add.sku_placeholder")}
                    required
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">{t("add.description")}</Label>
                <Textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => handleInputChange("description", e.target.value)}
                  placeholder={t("add.description_placeholder")}
                  rows={3}
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="category">{t("add.category")} *</Label>
                  <Select value={formData.category} onValueChange={(value) => handleInputChange("category", value)}>
                    <SelectTrigger>
                      <SelectValue placeholder={t("add.select_category")} />
                    </SelectTrigger>
                    <SelectContent>
                      {categories.map((category) => (
                        <SelectItem key={category} value={category}>
                          {category}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="location">Location *</Label>
                  <Select value={formData.location} onValueChange={(value) => handleInputChange("location", value)}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select location" />
                    </SelectTrigger>
                    <SelectContent>
                      {locations.map((location) => (
                        <SelectItem key={location} value={location}>
                          {location}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Inventory & Pricing */}
          <Card>
            <CardHeader>
              <CardTitle>{t("add.inventory_pricing")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="stock">Current Stock</Label>
                <Input
                  id="stock"
                  type="number"
                  value={formData.stock}
                  onChange={(e) => handleInputChange("stock", e.target.value)}
                  placeholder="0"
                  min="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="minStock">Minimum Stock</Label>
                <Input
                  id="minStock"
                  type="number"
                  value={formData.minStock}
                  onChange={(e) => handleInputChange("minStock", e.target.value)}
                  placeholder="0"
                  min="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="price">Cost Price</Label>
                <Input
                  id="price"
                  type="number"
                  step="0.01"
                  value={formData.price}
                  onChange={(e) => handleInputChange("price", e.target.value)}
                  placeholder="0.00"
                  min="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="sellPrice">Sell Price</Label>
                <Input
                  id="sellPrice"
                  type="number"
                  step="0.01"
                  value={formData.sellPrice}
                  onChange={(e) => handleInputChange("sellPrice", e.target.value)}
                  placeholder="0.00"
                  min="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="supplier">Supplier</Label>
                <Input
                  id="supplier"
                  value={formData.supplier}
                  onChange={(e) => handleInputChange("supplier", e.target.value)}
                  placeholder="Enter supplier name"
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Additional Notes */}
        <Card>
          <CardHeader>
            <CardTitle>Additional Information</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <Label htmlFor="notes">Notes</Label>
              <Textarea
                id="notes"
                value={formData.notes}
                onChange={(e) => handleInputChange("notes", e.target.value)}
                placeholder="Any additional notes or specifications"
                rows={3}
              />
            </div>
          </CardContent>
        </Card>

        {/* Action Buttons */}
        <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" asChild>
              <Link to="/dashboard/articles">{t("add.cancel")}</Link>
            </Button>
            <Button type="submit" disabled={!isFormValid}>
              <Save className="h-4 w-4 mr-2" />
              {t("add.save_article")}
            </Button>
        </div>
      </form>
      )}
    </div>
  );
};

export default AddArticle;